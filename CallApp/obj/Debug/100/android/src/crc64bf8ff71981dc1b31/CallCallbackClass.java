package crc64bf8ff71981dc1b31;


public class CallCallbackClass
	extends android.telecom.Call.Callback
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onStateChanged:(Landroid/telecom/Call;I)V:GetOnStateChanged_Landroid_telecom_Call_IHandler\n" +
			"";
		mono.android.Runtime.register ("CallApp.CallCallbackClass, CallApp", CallCallbackClass.class, __md_methods);
	}


	public CallCallbackClass ()
	{
		super ();
		if (getClass () == CallCallbackClass.class)
			mono.android.TypeManager.Activate ("CallApp.CallCallbackClass, CallApp", "", this, new java.lang.Object[] {  });
	}


	public void onStateChanged (android.telecom.Call p0, int p1)
	{
		n_onStateChanged (p0, p1);
	}

	private native void n_onStateChanged (android.telecom.Call p0, int p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
