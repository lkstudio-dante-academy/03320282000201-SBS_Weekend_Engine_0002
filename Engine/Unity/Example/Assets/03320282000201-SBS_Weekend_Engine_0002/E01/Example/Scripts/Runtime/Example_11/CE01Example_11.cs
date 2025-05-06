using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** Example 11 */
public class CE01Example_11 : CSceneManager
{
	#region 변수
	[Header("=====> UIs <=====")]
	[SerializeField] private Text m_oScoreText = null;
	#endregion // 변수

	#region 프로퍼티
	public override string SceneName => KDefine.G_SCENE_N_EXAMPLE_11;
	#endregion // 프로퍼티

	#region 함수
	/** 초기화 */
	public override void Awake()
	{
		base.Awake();
		this.UpdateUIsState();
	}

	/** UI 상태를 갱신한다 */
	private void UpdateUIsState()
	{
		m_oScoreText.text = $"점수 : {CE01DataStorage_10.Inst.Score}";
	}

	/** 다시하기 버튼을 눌렀을 경우 */
	public void OnTouchRetryBtn()
	{
		CSceneLoader.Inst.LoadScene(KDefine.G_SCENE_N_EXAMPLE_10);
	}

	/** 그만두기 버튼을 눌렀을 경우 */
	public void OnTouchLeaveBtn()
	{
		CSceneLoader.Inst.LoadScene(KDefine.G_SCENE_N_EXAMPLE_09);
	}
	#endregion // 함수
}
