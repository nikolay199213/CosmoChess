package com.cosmochess.ui.auth

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Toast
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.google.android.material.button.MaterialButton
import com.google.android.material.textfield.TextInputEditText
import kotlinx.coroutines.launch

class RegisterFragment : Fragment() {

    private val authRepository by lazy {
        (requireActivity().application as ChessApplication).authRepository
    }

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_register, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val usernameEdit = view.findViewById<TextInputEditText>(R.id.usernameEdit)
        val emailEdit = view.findViewById<TextInputEditText>(R.id.emailEdit)
        val passwordEdit = view.findViewById<TextInputEditText>(R.id.passwordEdit)
        val registerButton = view.findViewById<MaterialButton>(R.id.registerButton)
        val loginLink = view.findViewById<View>(R.id.loginLink)
        val progressBar = view.findViewById<View>(R.id.progressBar)

        registerButton.setOnClickListener {
            val username = usernameEdit.text.toString().trim()
            val email = emailEdit.text.toString().trim()
            val password = passwordEdit.text.toString().trim()

            if (username.isEmpty() || email.isEmpty() || password.isEmpty()) {
                Toast.makeText(context, "Please fill all fields", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            progressBar.visibility = View.VISIBLE
            registerButton.isEnabled = false

            lifecycleScope.launch {
                val result = authRepository.register(username, email, password)

                progressBar.visibility = View.GONE
                registerButton.isEnabled = true

                if (result.isSuccess) {
                    Toast.makeText(context, "Registration successful!", Toast.LENGTH_SHORT).show()
                    findNavController().navigate(R.id.action_register_to_menu)
                } else {
                    Toast.makeText(
                        context,
                        result.exceptionOrNull()?.message ?: getString(R.string.error_register),
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }

        loginLink.setOnClickListener {
            findNavController().navigateUp()
        }
    }
}
