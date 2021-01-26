package com.companyname.callapp;


public class InCallServiceImplementation
	extends android.telecom.InCallService
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCallAdded:(Landroid/telecom/Call;)V:GetOnCallAdded_Landroid_telecom_Call_Handler\n" +
			"n_onCallRemoved:(Landroid/telecom/Call;)V:GetOnCallRemoved_Landroid_telecom_Call_Handler\n" +
			"";
		mono.android.Runtime.register ("CallApp.InCallServiceClass, CallApp", InCallServiceImplementation.class, __md_methods);
	}


	public InCallServiceImplementation ()
	{
		super ();
		if (getClass () == InCallServiceImplementation.class)
			mono.android.TypeManager.Activate ("CallApp.InCallServiceClass, CallApp", "", this, new java.lang.Object[] {  });
	}


	public void onCallAdded (android.telecom.Call p0)
	{
		n_onCallAdded (p0);
	}

	private native void n_onCallAdded (android.telecom.Call p0);


	public void onCallRemoved (android.telecom.Call p0)
	{
		n_onCallRemoved (p0);
	}

	private native void n_onCallRemoved (android.telecom.Call p0);

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
