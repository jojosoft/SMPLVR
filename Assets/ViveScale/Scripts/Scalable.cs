/*
 * Author: Johannes Schirm, MPI for Biological Cybernetics
 * In case of questions: johannes.schirm@tuebingen.mpg.de
 */

using UnityEngine;
using System.Collections;

namespace ViveScale
{
	/// <summary>
	/// This interface summs up everything a scalable object needs to provide.
	/// The ViveScaler will only communicate with scalable objects.
	/// </summary>
	public interface Scalable {

		/// <summary>
		/// This is called whenever the user started a new scaling process.
		/// You need this for implementing relative scaling where you depend on the initial state.
		/// </summary>
		void StartScaling();

		/// <summary>
		/// The main function of this interface, which waits for new scaling values.
		/// Important: The scaling values are not limited to 100 % and can get larger than that.
		/// </summary>
		/// <param name="newScale">The new scale in percent.</param>
		void ReceiveNewScale(float newScale);
	}
}