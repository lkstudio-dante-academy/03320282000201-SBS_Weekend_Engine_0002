using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** 플레이어 */
public class CE01Player_18 : CComponent
{
	/** 무기 종류 */
	public enum EWeaponKinds
	{
		NONE = -1,
		RIFLE,
		SHOTGUN,
		[HideInInspector] MAX_VAL
	}

	/** 능력치 종류 */
	public enum EAbilityKinds
	{
		NONE = -1,
		HP,
		ATK,
		[HideInInspector] MAX_VAL
	}

	#region 변수
	private bool m_bIsDirtyUpdate = true;
	private EWeaponKinds m_eCurWeaponKinds = EWeaponKinds.RIFLE;

	private Dictionary<EAbilityKinds, int> m_oAbilityValDict = new Dictionary<EAbilityKinds, int>();
	private Dictionary<EAbilityKinds, int> m_oOriginAbilityValDict = new Dictionary<EAbilityKinds, int>();

	private Animation m_oAnimation = null;
	private CharacterController m_oController = null;

	[Header("=====> UIs <=====")]
	[SerializeField] private Image m_oHPImg = null;

	[Header("=====> Game Objects <=====")]
	[SerializeField] private GameObject m_oHPUIs = null;
	[SerializeField] private GameObject m_oBulletRoot = null;
	[SerializeField] private List<GameObject> m_oWeaponList = new List<GameObject>();
	#endregion // 변수

	#region 프로퍼티
	public GameObject CurWeapon => m_oWeaponList[(int)m_eCurWeaponKinds];
	public GameObject CurMuzzleFlash => this.CurWeaponInfo.MuzzleFlash;

	public CE01WeaponInfo_18 CurWeaponInfo => this.CurWeapon.GetComponent<CE01WeaponInfo_18>();
	#endregion // 프로퍼티

	#region 함수
	/** 초기화 */
	public override void Awake()
	{
		base.Awake();
		CScheduleManager.Inst.AddComponent(this);

		m_oAnimation = this.GetComponentInChildren<Animation>();
		m_oController = this.GetComponentInChildren<CharacterController>();

		// 충돌 이벤트를 설정한다
		var oDispatcher = this.GetComponentInChildren<CTriggerDispatcher>();
		oDispatcher.EnterCallback = this.HandleOnTriggerEnter;

		// 능력치를 설정한다 {
		m_oAbilityValDict.TryAdd(EAbilityKinds.HP, 10);
		m_oAbilityValDict.TryAdd(EAbilityKinds.ATK, 1);

		m_oAbilityValDict.ExCopyTo(m_oOriginAbilityValDict, (_, a_nVal) => a_nVal);
		// 능력치를 설정한다 }
	}

	/** 초기화 */
	public override void Start()
	{
		base.Start();
		this.GetSceneManager().SetPlayer(this);

		var oRectTrans = m_oHPUIs.transform as RectTransform;
		LayoutRebuilder.ForceRebuildLayoutImmediate(oRectTrans);
	}

	/** 상태를 갱신한다 */
	public override void OnUpdate(float a_fDeltaTime)
	{
		base.OnUpdate(a_fDeltaTime);

		this.UpdateShootState(a_fDeltaTime);
		this.UpdateWeaponState(a_fDeltaTime);

		float fVertical = Input.GetAxis("Vertical");
		float fHorizontal = Input.GetAxis("Horizontal");

		this.UpdateTransformState(fVertical,
			fHorizontal, a_fDeltaTime);

		this.UpdateAnimationState(fVertical,
			fHorizontal, a_fDeltaTime);
	}

	/** 상태를 갱신한다 */
	public override void OnLateUpdate(float a_fDeltaTime)
	{
		base.OnLateUpdate(a_fDeltaTime);

		// 상태 갱신이 필요 할 경우
		if(m_bIsDirtyUpdate)
		{
			m_bIsDirtyUpdate = false;
			this.UpdateUIsState();
		}
	}

	/** UI 상태를 갱신한다 */
	public void UpdateUIsState()
	{
		int nHP = m_oAbilityValDict[EAbilityKinds.HP];
		int nMaxHP = m_oOriginAbilityValDict[EAbilityKinds.HP];

		var stPos = m_oHPImg.rectTransform.anchoredPosition;
		float fPercent = nHP / (float)nMaxHP;

		m_oHPImg.fillAmount = fPercent;
		m_oHPImg.rectTransform.anchoredPosition = new Vector2(fPercent * m_oHPImg.rectTransform.rect.size.x, stPos.y);
	}

	/** 발사 상태를 갱신한다 */
	private void UpdateShootState(float a_fDeltaTime)
	{
		// 발사 키를 누른 상태가 아닐 경우
		if(!Input.GetKeyDown(KeyCode.Space))
		{
			return;
		}

		StopCoroutine("CoUpdateMuzzleFlashState");
		StartCoroutine(this.CoUpdateMuzzleFlashState());

		for(int i = 0; i < this.CurWeaponInfo.NumBulletsAtOnce; ++i)
		{
			var oBullet = this.CreateBullet();
			oBullet.Init(CE01Bullet_18.MakeParams(m_oAbilityValDict));
			oBullet.transform.position = this.CurWeaponInfo.BulletSpawnPos.transform.position;

			var oDispatcher = oBullet.GetComponentInChildren<CCollisionDispatcher>();
			oDispatcher.EnterCallback = this.HandleOnCollisionEnter;

			switch(this.m_eCurWeaponKinds)
			{
				case EWeaponKinds.RIFLE:
					this.UpdateShootStateRifle(a_fDeltaTime, oBullet);
					break;

				case EWeaponKinds.SHOTGUN:
					this.UpdateShootStateShotgun(a_fDeltaTime, oBullet);
					break;
			}
		}
	}

	/** 소총 발사 상태를 갱신한다 */
	private void UpdateShootStateRifle(float a_fDeltaTime,
		CE01Bullet_18 a_oBullet)
	{

		a_oBullet.transform.rotation = this.transform.rotation;
		a_oBullet.Shoot(this.transform.forward * this.CurWeaponInfo.ShootPower);
	}

	/** 샷건 발상 상태를 갱신한다 */
	private void UpdateShootStateShotgun(float a_fDeltaTime,
		CE01Bullet_18 a_oBullet)
	{

		var stCenter = a_oBullet.transform.position +
			(this.transform.forward * 15.0f);

		float fAngle = Random.Range(0.0f, 360.0f);

		var stRotation = Quaternion.AngleAxis(fAngle,
			this.transform.forward);

		var stDirection = stRotation * this.transform.right;
		stDirection *= Random.Range(0.0f, 7.5f);

		var stShootPos = stCenter + stDirection;
		var stShootDirection = stShootPos - a_oBullet.transform.position;

		a_oBullet.transform.forward = stShootDirection.normalized;
		a_oBullet.Shoot(stShootDirection.normalized * this.CurWeaponInfo.ShootPower);
	}

	/** 무기 상태를 갱신한다 */
	private void UpdateWeaponState(float a_fDeltaTime)
	{
		// 소총 장착 키를 눌렀을 경우
		if(Input.GetKeyDown(KeyCode.Alpha1))
		{
			m_eCurWeaponKinds = EWeaponKinds.RIFLE;
		}
		// 샷건 장착 키를 눌렀을 경우
		else if(Input.GetKeyDown(KeyCode.Alpha2))
		{
			m_eCurWeaponKinds = EWeaponKinds.SHOTGUN;
		}

		for(int i = 0; i < m_oWeaponList.Count; ++i)
		{
			m_oWeaponList[i].SetActive(i == (int)m_eCurWeaponKinds);
		}
	}

	/** 변환 상태를 갱신한다 */
	private void UpdateTransformState(float a_fVertical,
		float a_fHorizontal, float a_fDeltaTime)
	{

		var stDirection = (this.transform.forward * a_fVertical) +
			(this.transform.right * a_fHorizontal);

		// 보정이 필요 할 경우
		if(stDirection.magnitude >= 1.0f - float.Epsilon)
		{
			stDirection = stDirection.normalized;
		}

		m_oController.Move(stDirection * 350.0f * a_fDeltaTime);

		// 마우스 버튼을 눌렀을 경우
		if(Input.GetMouseButton((int)EMouseBtn.LEFT))
		{
			float fMouseX = Input.GetAxis("Mouse X");

			this.transform.Rotate(Vector3.up,
				fMouseX * 180.0f * a_fDeltaTime, Space.World);
		}
	}

	/** 애니메이션 상태를 갱신한다 */
	private void UpdateAnimationState(float a_fVertical,
		float a_fHorizontal, float a_fDeltaTime)
	{

		// 입력이 없을 경우
		if(a_fVertical.ExIsEquals(0.0f) && a_fHorizontal.ExIsEquals(0.0f))
		{
			m_oAnimation.CrossFade("Idle");
			return;
		}

		// 전/후방으로 이동했을 경우
		if(!a_fVertical.ExIsEquals(0.0f))
		{
			m_oAnimation.CrossFade((a_fVertical >= float.Epsilon) ?
				"RunF" : "RunB");
		}

		// 좌/우로 이동했을 경우
		if(!a_fHorizontal.ExIsEquals(0.0f))
		{
			m_oAnimation.CrossFade((a_fHorizontal >= float.Epsilon) ?
				"RunR" : "RunL");
		}
	}

	/** 충돌 시작을 처리한다 */
	private void HandleOnTriggerEnter(CTriggerDispatcher a_oSender,
		Collider a_oCollider)
	{

		// NPC 가 아닐 경우
		if(!a_oCollider.CompareTag("E18NonPlayer"))
		{
			return;
		}

		var oNonPlayer = a_oCollider.GetComponentInParent<CE01NonPlayer_18>();

		// 타격 가능 상태가 아닐 경우
		if(!oNonPlayer.IsEnableHit)
		{
			return;
		}

		oNonPlayer.IsEnableHit = false;

		int nHP = m_oAbilityValDict[EAbilityKinds.HP];
		int nDamage = oNonPlayer.ATK;

		m_bIsDirtyUpdate = true;
		m_oAbilityValDict[EAbilityKinds.HP] = Mathf.Max(0, nHP - nDamage);

		// 체력이 없을 경우
		if(m_oAbilityValDict[EAbilityKinds.HP] <= 0)
		{
			this.GetSceneManager().OnDeathPlayer();
		}
	}

	/** 충돌 시작을 처리한다 */
	private void HandleOnCollisionEnter(CCollisionDispatcher a_oSender,
		Collision a_oCollision)
	{

		a_oSender.TryGetComponent(out CE01NonPlayer_18 oNonPlayerA);
		a_oCollision.gameObject.TryGetComponent(out CE01NonPlayer_18 oNonPlayerB);

		oNonPlayerA?.OnHit(m_oAbilityValDict[EAbilityKinds.ATK],
			this.HandleOnDeathNonPlayer);

		oNonPlayerB?.OnHit(m_oAbilityValDict[EAbilityKinds.ATK],
			this.HandleOnDeathNonPlayer);

		int nBulletLayer = LayerMask.NameToLayer("E18Bullet");

		var oBullet = (a_oSender.gameObject.layer == nBulletLayer) ?
			a_oSender.gameObject : a_oCollision.gameObject;

		var oSceneManager = CSceneManager.GetSceneManager<CE01Example_18>(KDefine.G_SCENE_N_EXAMPLE_18);
		oSceneManager.GameObjsPoolManager.DespawnGameObj(typeof(CE01Bullet_18).ToString(), oBullet);
	}

	/** NPC 사망을 처리한다 */
	private void HandleOnDeathNonPlayer(CE01NonPlayer_18 a_oSender)
	{
		CE01DataStorage_18.Inst.NumDefeatNonPlayers += 1;
	}
	#endregion // 함수

	#region 접근 함수
	/** 씬 관리자를 반환한다 */
	public CE01Example_18 GetSceneManager()
	{
		return CSceneManager.GetSceneManager<CE01Example_18>(KDefine.G_SCENE_N_EXAMPLE_18);
	}
	#endregion // 접근 함수

	#region 팩토리 함수
	/** 총알을 생성한다 */
	private CE01Bullet_18 CreateBullet()
	{
		var oSceneManager = CSceneManager.GetSceneManager<CE01Example_18>(KDefine.G_SCENE_N_EXAMPLE_18);

		var oBullet = oSceneManager.GameObjsPoolManager.SpawnGameObj(typeof(CE01Bullet_18).ToString(), () =>
		{
			return CFactory.CreateCloneGameObj("Bullet",
				this.CurWeaponInfo.OriginBullet, m_oBulletRoot);
		});

		return oBullet.GetComponentInChildren<CE01Bullet_18>();
	}
	#endregion // 팩토리 함수

	#region 코루틴 함수
	/** 총구 화염 상태를 갱신한다 */
	private IEnumerator CoUpdateMuzzleFlashState()
	{
		int nOffsetX = Random.Range(0, 2);
		int nOffsetY = Random.Range(0, 2);

		var stOffset = new Vector2(nOffsetX * 0.5f,
			nOffsetY * 0.5f);

		var oMaterial = this.CurWeaponInfo.MuzzleFlashMaterial;
		oMaterial.SetTextureOffset("_MainTex", stOffset);

		this.CurMuzzleFlash.SetActive(true);
		yield return new WaitForSeconds(0.05f);

		this.CurMuzzleFlash.SetActive(false);
	}
	#endregion // 코루틴 함수
}

