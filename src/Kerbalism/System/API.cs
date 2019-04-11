﻿// ====================================================================================================================
// Functions that other mods can call to interact with Kerbalism. If you don't build against it, call these using
// reflection. All functions work transparently with loaded and unloaded vessels, unless otherwise specified.
// ====================================================================================================================


using System;
using System.Collections.Generic;

namespace KERBALISM
{
	public class AntennaInfo
	{
		/// <summary> science data rate. note that internal transmitters can not transmit science data only telemetry data </summary>
		public double rate = 0.0;

		/// <summary> ec cost </summary>
		public double ec = 0.0;

		public double strength = -1;

		/// <summary>
		/// direct_link = 0, indirect_link = 1 (relayed signal), no_link = 2, plasma = 3 (plasma blackout on reentry), storm = 4 (cme storm blackout)
		/// </summary>
		public int status = 2;

		public bool linked;

		public string target_name;

		public List<string[]> control_path = null;
	}

	public static class API
	{
		private static string config_location = "Kerbalism/Kerbalism";

		public static void SetConfigLocation(string location)
		{
			config_location = location;
		}

		public static string GetConfigLocation()
		{
			return config_location;
		}

		// show a message in the ui
		public static void Message(string msg)
		{
			KERBALISM.Message.Post(msg);
		}

		// kill a kerbal, even an EVA one
		public static void Kill(Vessel v, ProtoCrewMember c)
		{
			if (!Cache.VesselInfo(v).is_valid) return;
			if (!DB.vessels.ContainsKey(Lib.RootID(v))) return;
			if (!DB.ContainsKerbal(c.name)) return;
			Misc.Kill(v, c);
		}

		// trigger an undesiderable event for the kerbal specified
		public static void Breakdown(Vessel v, ProtoCrewMember c)
		{
			if (!Cache.VesselInfo(v).is_valid) return;
			if (!DB.vessels.ContainsKey(Lib.RootID(v))) return;
			if (!DB.ContainsKerbal(c.name)) return;
			Misc.Breakdown(v, c);
		}

		// disable or re-enable all rules for the specified kerbal
		public static void DisableKerbal(string k_name, bool disabled)
		{
			if (!DB.ContainsKerbal(k_name)) return;
			DB.Kerbal(k_name).disabled = disabled;
		}

		// inject instant radiation dose to the specified kerbal (can use negative amounts)
		public static void InjectRadiation(string k_name, double amount)
		{
			if (!DB.ContainsKerbal(k_name)) return;
			KerbalData kd = DB.Kerbal(k_name);
			foreach (Rule rule in Profile.rules)
			{
				if (rule.modifiers.Contains("radiation"))
				{
					RuleData rd = kd.rules[rule.name];
					rd.problem = Math.Max(rd.problem + amount, 0.0);
				}
			}
		}


		// --- ENVIRONMENT ----------------------------------------------------------

		// return true if the vessel specified is in sunlight
		public static bool InSunlight(Vessel v)
		{
			return Cache.VesselInfo(v).sunlight > double.Epsilon;
		}

		// return true if the vessel specified is inside a breathable atmosphere
		public static bool Breathable(Vessel v)
		{
			return Cache.VesselInfo(v).breathable;
		}


		// --- RADIATION ------------------------------------------------------------

		// return amount of environment radiation at the position of the specified vessel
		public static double Radiation(Vessel v)
		{
			if (!Features.Radiation) return 0.0;
			Vessel_info vi = Cache.VesselInfo(v);
			return vi.radiation;
		}

		// return true if the vessel is inside the magnetopause of some body (except the sun)
		public static bool Magnetosphere(Vessel v)
		{
			if (!Features.Radiation) return false;
			return Cache.VesselInfo(v).magnetosphere;
		}

		// return true if the vessel is inside the radiation belt of some body
		public static bool InnerBelt(Vessel v)
		{
			if (!Features.Radiation) return false;
			return Cache.VesselInfo(v).inner_belt;
		}

		// return true if the vessel is inside the radiation belt of some body
		public static bool OuterBelt(Vessel v)
		{
			if (!Features.Radiation) return false;
			return Cache.VesselInfo(v).outer_belt;
		}


		// --- SPACE WEATHER --------------------------------------------------------

		// return true if a solar storm is incoming at the vessel position
		public static bool StormIncoming(Vessel v)
		{
			if (!Features.SpaceWeather) return false;
			return Cache.VesselInfo(v).is_valid && Storm.Incoming(v);
		}

		// return true if a solar storm is in progress at the vessel position
		public static bool StormInProgress(Vessel v)
		{
			if (!Features.SpaceWeather) return false;
			return Cache.VesselInfo(v).is_valid && Storm.InProgress(v);
		}

		// return true if the vessel is subject to a signal blackout
		public static bool Blackout(Vessel v)
		{
			if (!RemoteTech.Enabled) return false;
			return Cache.VesselInfo(v).blackout;
		}

		// --- RELIABILITY ----------------------------------------------------------

		// return true if at least a component has malfunctioned, or had a critical failure
		public static bool Malfunction(Vessel v)
		{
			if (!Features.Reliability) return false;
			return Cache.VesselInfo(v).malfunction;
		}

		// return true if at least a componet had a critical failure
		public static bool Critical(Vessel v)
		{
			if (!Features.Reliability) return false;
			return Cache.VesselInfo(v).critical;
		}

		// return true if the part specified has a malfunction or critical failure
		public static bool Broken(Part part)
		{
			return part.FindModulesImplementing<Reliability>().FindAll(k => k.isEnabled && k.broken) != null;
		}

		// repair a specified part
		public static void Repair(Part part)
		{
			part.FindModulesImplementing<Reliability>().FindAll(k => k.isEnabled && k.broken).ForEach(k => k.Repair());
		}


		// --- HABITAT --------------------------------------------------------------

		// return volume of internal habitat in m^3
		public static double Volume(Vessel v)
		{
			if (!Features.Habitat) return 0.0;
			return Cache.VesselInfo(v).volume;
		}

		// return surface of internal habitat in m^2
		public static double Surface(Vessel v)
		{
			if (!Features.Habitat) return 0.0;
			return Cache.VesselInfo(v).surface;
		}

		// return normalized pressure of internal habitat
		public static double Pressure(Vessel v)
		{
			if (!Features.Pressure) return 0.0;
			return Cache.VesselInfo(v).pressure;
		}

		// return level of co2 of internal habitat
		public static double Poisoning(Vessel v)
		{
			if (!Features.Poisoning) return 0.0;
			return Cache.VesselInfo(v).poisoning;
		}

		// return level of co2 of internal habitat
		public static double Humidity(Vessel v)
		{
			if (!Features.Humidity)
				return 0.0;
			return Cache.VesselInfo(v).humidity;
		}

		// return proportion of radiation blocked by shielding
		public static double Shielding(Vessel v)
		{
			return Cache.VesselInfo(v).shielding;
		}

		// return living space factor
		public static double LivingSpace(Vessel v)
		{
			return Cache.VesselInfo(v).living_space;
		}

		// return comfort factor
		public static double Comfort(Vessel v)
		{
			return Cache.VesselInfo(v).comforts.factor;
		}


		// --- SCIENCE --------------------------------------------------------------

		// return size of a file in a vessel drive
		public static double FileSize(Vessel v, string subject_id)
		{
			if (!Cache.VesselInfo(v).is_valid) return 0.0;

			foreach (var d in DB.Vessel(v).drives.Values)
			{
				if (d.files.ContainsKey(subject_id))
					return d.files[subject_id].size;
			}

			return 0.0;
		}

		// return size of a sample in a vessel drive
		public static double SampleSize(Vessel v, string subject_id)
		{
			if (!Cache.VesselInfo(v).is_valid) return 0.0;
			foreach (var d in DB.Vessel(v).drives.Values)
			{
				if (d.samples.ContainsKey(subject_id))
					return d.samples[subject_id].size;
			}

			return 0.0;
		}

		// store a file on a vessel
		public static bool StoreFile(Vessel v, string subject_id, double amount)
		{
			if (!Cache.VesselInfo(v).is_valid) return false;
			return DB.Vessel(v).FileDrive(amount).Record_file(subject_id, amount);
		}

		// store a sample on a vessel
		public static bool StoreSample(Vessel v, string subject_id, double amount, double mass = 0)
		{
			if (!Cache.VesselInfo(v).is_valid) return false;
			return DB.Vessel(v).SampleDrive(amount, subject_id).Record_sample(subject_id, amount, mass);
		}

		// remove a file from a vessel
		public static void RemoveFile(Vessel v, string subject_id, double amount)
		{
			if (!Cache.VesselInfo(v).is_valid) return;
			foreach (var d in DB.Vessel(v).drives.Values)
				d.Delete_file(subject_id, amount);
		}

		// remove a sample from a vessel
		public static double RemoveSample(Vessel v, string subject_id, double amount)
		{
			if (!Cache.VesselInfo(v).is_valid) return 0;
			double massRemoved = 0;
			foreach (var d in DB.Vessel(v).drives.Values)
				massRemoved += d.Delete_sample(subject_id, amount);
			return massRemoved;
		}

		public static ScienceEvent OnScienceReceived = new ScienceEvent();

		public class ScienceEvent
		{
			//This is the list of methods that should be activated when the event fires
			private List<Action<float, ScienceSubject, ProtoVessel, bool>> listeningMethods = new List<Action<float, ScienceSubject, ProtoVessel, bool>>();

			//This adds an event to the List of listening methods
			public void Add(Action<float, ScienceSubject, ProtoVessel, bool> method)
			{
				//We only add it if it isn't already added. Just in case.
				if (!listeningMethods.Contains(method))
				{
					listeningMethods.Add(method);
				}
			}

			//This removes and event from the List
			public void Remove(Action<float, ScienceSubject, ProtoVessel, bool> method)
			{
				//We also only remove it if it's actually in the list.
				if (listeningMethods.Contains(method))
				{
					listeningMethods.Remove(method);
				}
			}

			//This fires the event off, activating all the listening methods.
			public void Fire(float credits, ScienceSubject subject, ProtoVessel pv, bool transmitted)
			{
				//Loop through the list of listening methods and Invoke them.
				foreach (Action<float, ScienceSubject, ProtoVessel, bool> method in listeningMethods)
				{
					method.Invoke(credits, subject, pv, transmitted);
				}
			}
		}

		// --- FAILURES --------------------------------------------------------------

		public static FailureInfo Failure = new FailureInfo();

		public class FailureInfo
		{
			//This is the list of methods that should be activated when the event fires
			internal List<Action<Part, string, bool>> receivers = new List<Action<Part, string, bool>>();

			//This adds a connection info handler
			public void Add(Action<Part, string, bool> receiver)
			{
				//We only add it if it isn't already added. Just in case.
				if (!receivers.Contains(receiver))
				{
					receivers.Add(receiver);
				}
			}

			//This removes a connection info handler
			public void Remove(Action<Part, string, bool> receiver)
			{
				//We also only remove it if it's actually in the list.
				if (receivers.Contains(receiver))
				{
					receivers.Remove(receiver);
				}
			}

			public void Notify(Part part, string type, bool failure)
			{
				//Loop through the list of listening methods and Invoke them.
				foreach (Action<Part, string, bool> receiver in receivers)
				{
					receiver.Invoke(part, type, failure);
				}
			}
		}

		// --- COMMUNICATION --------------------------------------------------------------

		public static CommInfo Comm = new CommInfo();

		public class CommInfo
		{
			//This is the list of methods that should be activated when the event fires
			internal List<Action<AntennaInfo, Vessel, bool, bool>> handlers = new List<Action<AntennaInfo, Vessel, bool, bool>>();

			//This adds a connection info handler
			public void Add(Action<AntennaInfo, Vessel, bool, bool> handler)
			{
				//We only add it if it isn't already added. Just in case.
				if (!handlers.Contains(handler))
				{
					handlers.Add(handler);
				}
			}

			//This removes a connection info handler
			public void Remove(Action<AntennaInfo, Vessel, bool, bool> handler)
			{
				//We also only remove it if it's actually in the list.
				if (handlers.Contains(handler))
				{
					handlers.Remove(handler);
				}
			}

			//This initializes an antennaInfo object. Connection info handlers must
			//set antennaInfo.strength to a value >= 0, otherwise the antennaInfo will
			//be passed to the next handler.
			public void Init(AntennaInfo antennaInfo, Vessel pv, bool powered, bool storm)
			{
				//Loop through the list of listening methods and Invoke them.
				foreach (Action<AntennaInfo, Vessel, bool, bool> handler in handlers)
				{
					handler.Invoke(antennaInfo, pv, powered, storm);
					if (antennaInfo.strength > -1) return;
				}
			}
		}
	}
} // KERBALISM

