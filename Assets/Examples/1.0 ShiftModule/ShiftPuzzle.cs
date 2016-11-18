using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ShiftPuzzle : MonoBehaviour
{
	public KMSelectable[] buttons;
	public Transform[] visibleButtons;
	Transform[] transforms;
	bool isActive;
	int[] field = new int[9] {1,2,3,4,5,6,7,8,0};
	int skull, knight, winCond, batteryCount;
	bool inDanger;
	int[,] table = new int[8,8] 
	   {{1,3,5,4,6,7,2,8},
		{2,5,7,3,8,1,4,6},
		{6,4,8,1,7,3,5,2},
		{8,1,2,5,3,4,6,7},
		{3,2,6,8,4,5,7,1},
		{7,6,1,2,5,8,3,4},
		{4,7,3,6,1,2,8,5},
		{5,8,4,7,2,6,1,3}};

	int sNumber;

	//float emptyX = 0.0866f;
	//float emptyZ = -0.0848f;
	Transform emptyField;
	Renderer background;

	void Start()
	{
		Init();
	}

	void Init()
	{
		for(int i = 0; i < buttons.Length; i++)
		{
			int j = i;
			if (buttons [i].gameObject.name != "empty")
				buttons [i].OnInteract += delegate () {
					OnPress (j);
					return false;
				};
			else
				Debug.Log ("empty found");
		}

		Random.seed = (int)System.DateTime.Now.Ticks;

		GetComponent<KMBombModule>().OnActivate += OnActivate;
		transforms = this.GetComponentsInChildren<Transform> ();

		foreach (Transform t in transforms) {
			if (t.gameObject.name == "empty") {
				emptyField = t;
			}
		}
			
		sNumber = 0;
		inDanger = true;
		skull = 0;
		knight = 0;



		initArray ();
		Debug.Log ("initArray complete");

		winCond = findWinCondition ();

		Debug.Log("table:"+table[1,2]);
		//setStartField1 ();
		setStartField2();

		Debug.Log ("setStartField complete");


		/*for (int i = 0; i <= 8; i++) {
			if (field [i] == 0)
				GetComponent<KMSelectable> ().Children [i] = buttons [8];
			else {
				GetComponent<KMSelectable> ().Children [i] = buttons [field [i] - 1];
				Debug.Log ("Button" + i + ":  " + GetComponent<KMSelectable> ().Children [i].gameObject.name);
			}
		}*/
	}

	int findWinCondition(){
		int w = 0;
		if (topSum () > horMidSum () && topSum () > bottomSum ())
			w += 0;
		else if (horMidSum () > topSum () && horMidSum () > bottomSum ())
			w += 10;
		else if (bottomSum () > topSum () && bottomSum () > horMidSum ())
			w += 20;
		else
			w += 30;
		if (leftSum () > verMidSum () && leftSum () > rightSum ())
			w += 0;
		else if (verMidSum () > leftSum () && verMidSum () > rightSum ())
			w += 1;
		else if (rightSum () > leftSum () && rightSum() > verMidSum ())
			w += 2;
		else
			w += 3;
		return w;

	}

	void OnActivate()
	{
		foreach (string query in new List<string> { KMBombInfo.QUERYKEY_GET_BATTERIES, KMBombInfo.QUERYKEY_GET_INDICATOR, KMBombInfo.QUERYKEY_GET_PORTS, KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, "example"})
		{
			List<string> queryResponse = GetComponent<KMBombInfo>().QueryWidgets(query, null);

			if (queryResponse.Count > 0)
			{
				Debug.Log(queryResponse[0]);
			}
		}

		int batteryCount = 0;
		List<string> responses = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_BATTERIES, null);
		foreach (string response in responses)
		{
			Dictionary<string, int> responseDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(response);
			batteryCount += responseDict["numbatteries"];
		}

		Debug.Log("Battery count: " + batteryCount);

		string serial = "";
		List<string> serialResponses = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
		Dictionary<string, string> serialDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(serialResponses[0]);
		serial = serialDict ["serial"];
		sNumber = (int)serial [5]-48;
		Debug.Log("Serial: "+serial+ "          sNumber: "+sNumber);
			
		//if (sNumber > 6)
		//	GetComponent<KMBombModule>().HandlePass ();

		Debug.Log("Battery count: " + batteryCount);

		placeObjectives ();
		isActive = true;
	}

	void placeObjectives(){

		skull = 0;
		int verDif, horDif;

		for (int i = 0; i <= 8; i++) {
			if (field [i] == 0) {
				skull = i;
			}
		}

		int checkN;
		for (int k = 0; k <= 7; k++) {
			if(sNumber !=0&&(sNumber==field[0]||sNumber==field[2]||sNumber==field[4]||sNumber==field[6]||sNumber==field[8]))
				checkN = table [(field [4] ==0)?1:(field[4]-1), k];		//if midfield is empty
			else checkN = table [k,(field [4] ==0)?1:(field[4]-1)];

			Debug.Log("checkN: "+checkN);
			for (int i = 0; i <= 8; i++) {
				if (field [i] == checkN) {
					verDif = (int)i / 3 - (int)skull / 3;
					horDif = i % 3 - skull % 3;
					//Debug.Log ("hor: " + horDif + "     ver: " + verDif);
					if ((Mathf.Abs (horDif) + Mathf.Abs (verDif)) == 1) {
						skull = i;
						Debug.Log ("skull under: " + field[i]);
					}
					break;
				}

			}
		}

		//if midfield empty, skull under number 7
		if (field [4] == 0)
			placeSkullUnderNumber (7);

		knight = skull;
		while (knight == skull || field [knight] == 0) {
			knight = (int)(Random.value * 9);
			if (knight == 9)
				knight = 8;
		}

		//skull texture positioning
		int zOffset = 0;
		int xOffset =0;
		if (skull < 3)
			zOffset = 2;
		else if (skull < 6)
			zOffset = 1;
		if (skull == 1 || skull == 4 || skull == 7)
			xOffset = 1;
		else if (skull == 2 || skull == 5 || skull == 8)
			xOffset = 2;

		Renderer[] renderers;
		renderers = this.GetComponentsInChildren<Renderer> ();

		foreach (Renderer r in renderers) {
			
			if (r.gameObject.name == "RecessedInner") {
				background = r;
			}
		}

		background.materials[2].mainTextureOffset = new Vector2(-1.6f-xOffset*0.9f,-2f-zOffset*0.95f);

		//knight texture positioning
		zOffset = 0;
		xOffset = 0;
		if (knight < 3)
			zOffset = 2;
		else if (knight < 6)
			zOffset = 1;
		if (knight == 1 || knight == 4 || knight == 7)
			xOffset = 1;
		else if (knight == 2 || knight == 5 || knight == 8)
			xOffset = 2;
		background.materials[1].mainTextureOffset = new Vector2(-1.6f-xOffset*0.9f,-2f-zOffset*0.95f);


	}

	int topSum(){
		return field [0] + field [1] + field [2];
	}

	int horMidSum(){
		return field [3] + field [4] + field [5];
	}

	int bottomSum(){
		return field [6] + field [7] + field [8];
	}

	int leftSum(){
		return field [0] + field [3] + field [6];
	}

	int verMidSum(){
		return field [1] + field [4] + field [7];
	}

	int rightSum(){
		return field [2] + field [5] + field [8];
	}

	void placeSkullUnderNumber(int number){
		for(int i=0; i<=8;i++){
			if(field[i] == number) 
				skull=i;
		}
	}

	bool checkWin(){
		if (intArrayEquals (new int[]{ 1, 2, 3, 4, 5, 6, 7, 8, 0 }, field))
			return true;
		switch (winCond) {
		case 0:		//Corners 1,2,3,4
			if(field[0] == 1 && field[2] == 2 && field[6]==4 && field[8]==3)
				return true;
			break;
		case 1:		//Corners 1,2,4,3
			if(field[0] == 1 && field[2] == 2 && field[6]==3 && field[8]==4)
				return true;
			break;
		case 2:		//Corners 1,3,5,7
			if(field[0] == 1 && field[2] == 3 && field[6]==7 && field[8]==5)
				return true;
			break;
		case 3:		//Corners 1,3,7,5
			if(field[0] == 1 && field[2] == 3 && field[6]==5 && field[8]==7)
				return true;
			break;
		case 10:		//Sides 1,2,3,4
			if(field[1]==1 && field[5] == 2 && field[7]==3 && field[3]==4)
				return true;
			break;
		case 11:		//Sides 1,2,4,3
			if(field[1]==1 && field[5] == 2 && field[7]==4 && field[3]==3)
				return true;
			break;
		case 12:		//Sides 2,4,6,8
			if(field[1]==2 && field[5] == 4 && field[7]==6 && field[3]==8)
				return true;
			break;
		case 13:		//Sides 2,4,8,6
			if(field[1]==2 && field[5] == 4 && field[7]==8 && field[3]==6)
				return true;
			break;
		case 20:		//descdiag
			if (field [0] == 1 && field [4] == 2 && field [8] == 3)
				return true;
			break;
		case 21:
			if (field [6] == 1 && field [4] == 2 && field [2] == 3)
				return true;
			break;
		case 22:
			if (field [0] == 3 && field [4] == 2 && field [8] == 1)
				return true;
			break;
		case 23:
			if (field [6] == 3 && field [4] == 2 && field [2] == 1)
				return true;
			break;
		case 30:
			if (field [0] == 1 && field [1] == 2 && field [2] == 3&&field[4]==4&&field[7]==5)
				return true;
			break;
		case 31:
			if (field [0] == 1 && field [3] == 2 && field [6] == 3&&field[4]==4&&field[5]==5)
				return true;
			break;
		case 32:
			if (field [6] == 1 && field [7] == 2 && field [8] == 3&&field[4]==4&&field[1]==5)
				return true;
			break;
		case 33:
			if (field [2] == 1 && field [5] == 2 && field [8] == 3&&field[4]==4&&field[3]==5)
				return true;
			break;
		default:
			return false;
		}
		return false;
	}
		

	void setStartField2(){
		int[] start = new int[9]{ 1, 2, 3, 4, 5, 6, 7, 8, 0 };
		Vector3 dif;
		int temp;

		for (int i = 0; i <= 8; i++) {
			for (int j = 0; j <= 8; j++) {
				if (start [j] == field [i]) {
					if (start [j] == 0) {
						if (start [i] != 0) {
							dif = visibleButtons [start [i] - 1].gameObject.transform.position - emptyField.transform.position;
							visibleButtons [start [i] - 1].gameObject.transform.Translate (-dif, Space.World);
							emptyField.transform.Translate (dif, Space.World);
						}
					} else if (start [i] != 0) {
						dif = visibleButtons [start [i] - 1].gameObject.transform.position - visibleButtons [start [j] - 1].gameObject.transform.position;
						visibleButtons [start [i] - 1].gameObject.transform.Translate (-dif, Space.World);
						visibleButtons [start [j] - 1].gameObject.transform.Translate (dif, Space.World);
					} else {
						dif = emptyField.transform.position - visibleButtons [start [j] - 1].gameObject.transform.position;
						emptyField.transform.Translate (-dif, Space.World);
						visibleButtons [start [j] - 1].gameObject.transform.Translate (dif, Space.World);
					}
		
					temp = start [j];
					start [j] = start [i];
					start [i] = temp;
				}
			}
		}
	}

	void initArray(){
		for (int i = 0; i <= 8; i++) {
			field [i] = 10;
		}
		field [7] = 11;

		bool exists = true;
		int next = 0;


		while (oddPermutation ()) {

			for (int i = 0; i <= 8; i++) {
				field [i] = 10;
			}
			field [7] = 11;

			for (int i = 0; i <= 8; i++) {
				exists = true;

				int count = 0;

				while (exists == true) {
					count++;
					exists = false;
					next = (int)(Random.value * 9);
					Debug.Log ("next: " + next);
					if (next == 9)
						next = 0;
					for (int j = 0; j <= 8; j++) {
						if (field [j] == next)
							exists = true;
					}
					if (count > 100) {
						Debug.Log ("abort while");
						break;
					}
				}
				field [i] = next;
			}
		}
		Debug.Log ("Field: " + field [0] + field [1] + field [2] + field [3] + field [4] + field [5] + field [6] + field [7] + field [8]);
	}

	bool oddPermutation(){
		int permutations = 0;
		for (int i = 0; i <= 8; i++) {
			for (int j = 0; j < i; j++) {
				if (field [i] != 0 && field [j] != 0 && (field [i] < field [j]))
					permutations++;
			}
		}
		Debug.Log ("Permutations: " + permutations);
		return ((permutations % 2) == 0) ? false : true;

	}


	void setStartField1(){
	
		float xFac = -0.044f;
		float zFac = 0.042f;

		for (int i = 0; i <= 8; i++) {
			//buttons [i].gameObject.transform.Translate(0.05f, 0.0f, 0.0f);
			switch (field [i]) {
			case 1:
				buttons [0].gameObject.transform.Translate (xFac * (i % 3), 0.0f, zFac * (int)(i / 3), Space.Self);
				break;
			case 2:
				buttons [1].gameObject.transform.Translate (xFac * (i % 3 - 1), 0.0f, zFac * (int)(i / 3), Space.Self);
				break;
			case 3:
				buttons [2].gameObject.transform.Translate (xFac * (i % 3 - 2), 0.0f, zFac * (int)(i / 3), Space.Self);
				break;
			case 4:
				buttons [3].gameObject.transform.Translate (xFac * (i % 3), 0.0f, zFac * (int)(i / 3 - 1), Space.Self);
				break;
			case 5:
				buttons [4].gameObject.transform.Translate (xFac * (i % 3 - 1), 0.0f, zFac * (int)(i / 3 - 1), Space.Self);
				break;
			case 6:
				buttons [5].gameObject.transform.Translate (xFac * (i % 3 - 2), 0.0f, zFac * (int)(i / 3 - 1), Space.Self);
				break;
			case 7:
				buttons [6].gameObject.transform.Translate (xFac * (i % 3), 0.0f, zFac * (int)(i / 3 - 2), Space.Self);
				break;
			case 8:
				buttons [7].gameObject.transform.Translate (xFac * (i % 3 - 1), 0.0f, zFac * (int)(i / 3 - 2), Space.Self);
				break;
			case 0:
				emptyField.transform.Translate (xFac * (i % 3 - 2), 0.0f, zFac * (int)(i / 3 - 2), Space.Self);
				//emptyX = emptyX + xFac * (i % 3 - 2);
				//emptyZ = emptyZ + zFac * (int)(i / 3 - 2);
				break;
			default:
				break;
			}
		}
	}

	void checkDifuse(){
		//int[] accept;

		if (inDanger) {
			if (field [knight] == 0) {
				inDanger = false;
				Debug.Log ("Knight");
			}
			if (field [skull] == 0) {
				GetComponent<KMBombModule> ().HandleStrike ();
				Debug.Log ("Sktrike");
			}
		} else {
			//accept= new int[]{ 1, 2, 3, 4, 5, 6, 7, 8, 0 };
			if (checkWin()) {
				GetComponent<KMBombModule> ().HandlePass ();
				Debug.Log ("Defused");
			}
		}
	}

	bool intArrayEquals(int[] a, int[] b){
		if(a.Length == b.Length){
			for (int i = 0; i < a.Length; i++) {
				if (a [i] != b [i])
					return false;
			}
			return true;
		}
		return false;
	}


	void OnPress(int position)
	{
		if (true) {
			//Debug.Log ("Button: " + button);

			int empty = 0; 
			int verDif, horDif;
			Vector3 dif;

			for (int k = 0; k <= 8; k++) {
				if (field [k] == 0) {
					empty = k;
				}
			}

			int button = field [position]-1;
			if (button != 8) {
				int i;
				i = position;	//attetion
				verDif = (int)i / 3 - (int)empty / 3;
				horDif = i % 3 - empty % 3;
				Debug.Log ("hor: " + horDif + "     ver: " + verDif);
				if ((Mathf.Abs (horDif) + Mathf.Abs (verDif)) == 1) {
					field [i] = 0;
					field [empty] = button + 1;								//attention
					/*temp = emptyX;
					emptyX = buttons [button].gameObject.transform.localPosition.x;
					buttons [button].gameObject.transform.Translate (emptyX - temp, 0, 0, Space.Self);
					temp = emptyZ;
					emptyZ = buttons [button].gameObject.transform.localPosition.z;
					buttons [button].gameObject.transform.Translate (0, 0, emptyZ-temp, Space.Self);*/

					dif = visibleButtons [button].gameObject.transform.position - emptyField.transform.position;
					visibleButtons [button].gameObject.transform.Translate (-dif, Space.World);
					emptyField.transform.Translate (dif, Space.World);
				}

				checkDifuse ();
			}
		}
	}
		
}
