using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Stores each phase (scene) of test
public class Phase {

	private string name;
	private int time;
	private float size;

	public Phase (string name = "", int time = 0, float size = 4f)
	{
		this.name = name;
		this.time = time;
		this.size = size;
	}

	public string Name {
		get {
			return this.name;
		}
		set {
			name = value;
		}
	}

	public int Time {
		get {
			return this.time;
		}
		set {
			time = value;
		}
	}

	public float Size {
		get {
			return this.size;
		}
		set {
			size = value;
		}
	}
}
	
// Handles moving between scenes
public class Phases {

	enum PhaseNames {Practice1, Rest1, Learning, Rest2, Test};
	public enum PhaseTypes {CW4, Resetting2, Resetting3, Resetting4};

	private Phase[] myPhases;

	public Phases()
	{
		// ties size of myPhases to size of enum
		myPhases = new Phase[Enum.GetNames(typeof(PhaseNames)).Length];

		// Hardcode times
		myPhases [(int) PhaseNames.Practice1] = new Phase("", 300);
		myPhases [(int) PhaseNames.Rest1] = new Phase("", 6000); //cause why not
		myPhases [(int) PhaseNames.Learning] = new Phase("", 600);
		myPhases [(int) PhaseNames.Rest2] = new Phase("", 6000); //cause why not
		myPhases [(int) PhaseNames.Test] = new Phase("", int.MaxValue);
	}

	public Phase[] getPhases() {
		return this.myPhases;
	}

	// Set learning
	// First practice is same type as learning
	public void SetLearning(PhaseTypes t) {
		myPhases [(int) PhaseNames.Rest1].Name = "Rest Phase";
		if (t == PhaseTypes.CW4) {
			myPhases [(int) PhaseNames.Practice1].Name = "CW4 Practice Phase";
			myPhases [(int) PhaseNames.Practice1].Size = 0f;
			myPhases [(int) PhaseNames.Learning].Name = "CW4 Learning Phase";
			myPhases [(int) PhaseNames.Learning].Size = 0f;
		} else {
			myPhases [(int) PhaseNames.Practice1].Name = "Resetting 4x4 Practice Phase";
			myPhases [(int) PhaseNames.Learning].Name = "Resetting 4x4 Learning Phase";
			if (t == PhaseTypes.Resetting2) {
				myPhases [(int)PhaseNames.Practice1].Size = 2f;
				myPhases [(int)PhaseNames.Learning].Size = 2f;
			}else if (t == PhaseTypes.Resetting3) {
				myPhases [(int)PhaseNames.Practice1].Size = 3f;
				myPhases [(int)PhaseNames.Learning].Size = 3f;
			}else if (t == PhaseTypes.Resetting4) {
				myPhases [(int)PhaseNames.Practice1].Size = 4f;
				myPhases [(int)PhaseNames.Learning].Size = 4f;
			}
		}
	}

	// Set testing
	// Second practice is same type as testing
	public void SetTesting(PhaseTypes t) {
		myPhases [(int) PhaseNames.Rest2].Name = "Rest Phase";
		if (t == PhaseTypes.Resetting2) {
			myPhases [(int) PhaseNames.Test].Name = "Resetting 2x2 Test Phase";
		} else if (t == PhaseTypes.Resetting3) {
			myPhases [(int) PhaseNames.Test].Name = "Resetting 3x3 Test Phase";
		} else if (t == PhaseTypes.Resetting4) {
			myPhases [(int) PhaseNames.Test].Name = "Resetting 4x4 Test Phase";
		} else {
			myPhases [(int) PhaseNames.Test].Name = "CW4 Test Phase";
		}

		if (t == PhaseTypes.CW4) {
			myPhases [(int) PhaseNames.Test].Name = "CW4 Test Phase";
			myPhases [(int) PhaseNames.Test].Size = 0f;
		} else {
			myPhases [(int) PhaseNames.Test].Name = "Resetting 4x4 Test Phase";
			if (t == PhaseTypes.Resetting2) {
				myPhases [(int)PhaseNames.Test].Size = 2f;
			}else if (t == PhaseTypes.Resetting3) {
				myPhases [(int)PhaseNames.Test].Size = 3f;
			}else if (t == PhaseTypes.Resetting4) {
				myPhases [(int)PhaseNames.Test].Size = 4f;
			}
		}
	}
}