using System;
using System.Collections.Generic;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.Kaomoza
{
	[PluginFilter("CM3D2x64"), PluginFilter("CM3D2x86"), PluginName("Kaomoza"), PluginVersion("0.0.0.1")]
	public class Kaomoza : PluginBase
	{
		public class Config
		{
			public enum ShadingType
			{
				Plane,
				Mosaic,
			}

			public class MosaicObject
			{
				public ShadingType Shading = ShadingType.Plane;
				public float MosaicFiness = 15.0f;
				public PrimitiveType Primitive = PrimitiveType.Cube;
				public string Attach = string.Empty;
				public Vector3 Position = Vector3.zero;
				public Vector3 Rotation = Vector3.zero;
				public Vector3 Scale = Vector3.zero;
			}

			public KeyCode KeyApply = KeyCode.M;
			public List<MosaicObject> MosaicObjects = new List<MosaicObject>();
		}

		private static Config m_config = new Config();
		private static bool m_enabled = false;
		private static List<GameObject> m_objects = new List<GameObject>();

		public void Awake()
		{
			UnityEngine.Object.DontDestroyOnLoad(this);
		}

		public void OnLevelWasLoaded(int lv)
		{
			m_config = loadXml<Config>(System.IO.Path.Combine(this.DataPath, "Kaomoza.xml"));
		}

		public void Update()
		{
			if (Input.GetKeyDown(m_config.KeyApply))
			{
				m_enabled = !m_enabled;
				m_config = loadXml<Config>(System.IO.Path.Combine(this.DataPath, "Kaomoza.xml"));
				foreach (var go in m_objects) UnityEngine.Object.Destroy(go);
				m_objects.Clear();
			}

			if (!m_enabled) return;

			var cnt = GameMain.Instance.CharacterMgr.GetMaidCount();
			while (m_objects.Count < cnt * m_config.MosaicObjects.Count) { m_objects.Add(null); }

			for (int i = 0; i < cnt; ++i)
			{
				var md = GameMain.Instance.CharacterMgr.GetMaid(i);
				if (md == null || md.body0 == null || md.gameObject == null)
				{
					for (int j = 0; j < m_config.MosaicObjects.Count; ++j)
					{
						int idx = i * m_config.MosaicObjects.Count + j;
						if (m_objects[idx] != null)
						{
							UnityEngine.Object.Destroy(m_objects[idx]);
							m_objects[idx] = null;
						}
					}

					continue;
				}

				var trs = getChildren(md.gameObject.transform);

				for (int j = 0; j < m_config.MosaicObjects.Count; ++j)
				{
					int idx = i * m_config.MosaicObjects.Count + j;
					var obj = m_objects[idx];
					var cnf = m_config.MosaicObjects[j];

					GameObject attach = null;

					foreach (var tr in trs)
					{
						if (tr.gameObject.name.Contains(cnf.Attach))
						{
							attach = tr.gameObject;
							break;
						}
					}

					if (attach == null)
					{
						if (m_objects[idx] != null)
						{
							UnityEngine.Object.Destroy(m_objects[idx]);
							m_objects[idx] = null;
						}
						continue;
					}

					if (obj == null)
					{
						obj = GameObject.CreatePrimitive(cnf.Primitive);
						switch (cnf.Shading)
						{
							case Config.ShadingType.Plane:
								obj.GetComponent<MeshRenderer>().sharedMaterial.shader = Shader.Find("Diffuse");
								obj.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", Color.black);
								break;
							case Config.ShadingType.Mosaic:
								obj.GetComponent<MeshRenderer>().sharedMaterial.shader = Shader.Find("CM3D2/Mosaic");
								obj.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_FloatValue1", cnf.MosaicFiness);
								break;
						}

						obj.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 3500;
					}

					var cam = GameObject.Find("CameraMain");
					if (cam == null) continue;

					var dir = obj.transform.position - cam.transform.position;
					obj.transform.parent = attach.transform;
					obj.transform.localPosition = cnf.Position;
					obj.transform.localRotation = Quaternion.Euler(cnf.Rotation);
					obj.transform.localScale = cnf.Scale;

					m_objects[idx] = obj;
				}
			}
		}

		private T loadXml<T>(string path)
		{
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
			using (var sr = new System.IO.StreamReader(path, new System.Text.UTF8Encoding(true)))
			{
				return (T)serializer.Deserialize(sr);
			}
		}

		private List<Transform> getChildren(Transform parent)
		{
			List<Transform> ret = new List<Transform>();

			foreach (Transform child in parent)
			{
				ret.Add(child);
				ret.AddRange(getChildren(child));
			}

			return ret;
		}
	}
}