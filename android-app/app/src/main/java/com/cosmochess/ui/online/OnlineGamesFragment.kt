package com.cosmochess.ui.online

import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.core.os.bundleOf
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.cosmochess.ChessApplication
import com.cosmochess.R
import com.cosmochess.data.model.Game
import com.cosmochess.ui.game.WebViewGameActivity
import com.google.android.material.button.MaterialButton
import kotlinx.coroutines.launch

class OnlineGamesFragment : Fragment() {

    private val authRepository by lazy {
        (requireActivity().application as ChessApplication).authRepository
    }

    private val gameRepository by lazy {
        (requireActivity().application as ChessApplication).gameRepository
    }

    private lateinit var recyclerView: RecyclerView
    private lateinit var swipeRefresh: SwipeRefreshLayout
    private lateinit var emptyText: TextView
    private lateinit var progressBar: ProgressBar
    private lateinit var adapter: GamesAdapter

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_online_games, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        recyclerView = view.findViewById(R.id.gamesRecyclerView)
        swipeRefresh = view.findViewById(R.id.swipeRefresh)
        emptyText = view.findViewById(R.id.emptyText)
        progressBar = view.findViewById(R.id.progressBar)
        val createGameButton = view.findViewById<MaterialButton>(R.id.createGameButton)

        setupRecyclerView()
        setupSwipeRefresh()

        createGameButton.setOnClickListener {
            createNewGame()
        }

        loadGames()
    }

    private fun setupRecyclerView() {
        adapter = GamesAdapter { game ->
            joinGame(game)
        }
        recyclerView.layoutManager = LinearLayoutManager(requireContext())
        recyclerView.adapter = adapter
    }

    private fun setupSwipeRefresh() {
        swipeRefresh.setOnRefreshListener {
            loadGames()
        }
    }

    private fun loadGames() {
        if (!swipeRefresh.isRefreshing) {
            progressBar.visibility = View.VISIBLE
        }

        lifecycleScope.launch {
            val result = gameRepository.getWaitingGames()

            requireActivity().runOnUiThread {
                progressBar.visibility = View.GONE
                swipeRefresh.isRefreshing = false

                if (result.isSuccess) {
                    val games = result.getOrNull() ?: emptyList()
                    adapter.submitList(games)
                    emptyText.visibility = if (games.isEmpty()) View.VISIBLE else View.GONE
                } else {
                    Toast.makeText(
                        context,
                        result.exceptionOrNull()?.message ?: getString(R.string.error_network),
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    private fun createNewGame() {
        val user = authRepository.getCurrentUser()
        if (user == null) {
            Toast.makeText(context, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        progressBar.visibility = View.VISIBLE

        lifecycleScope.launch {
            val result = gameRepository.createGame(user.id)

            requireActivity().runOnUiThread {
                progressBar.visibility = View.GONE

                if (result.isSuccess) {
                    val gameId = result.getOrNull()!!
                    Toast.makeText(context, "Game created! Waiting for opponent...", Toast.LENGTH_SHORT).show()
                    // Launch WebView activity instead of fragment navigation
                    val intent = Intent(requireContext(), WebViewGameActivity::class.java).apply {
                        putExtra("gameId", gameId)
                    }
                    startActivity(intent)
                } else {
                    Toast.makeText(
                        context,
                        result.exceptionOrNull()?.message ?: getString(R.string.error_create_game),
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    private fun joinGame(game: Game) {
        val user = authRepository.getCurrentUser()
        if (user == null) {
            Toast.makeText(context, "Please login first", Toast.LENGTH_SHORT).show()
            return
        }

        progressBar.visibility = View.VISIBLE

        lifecycleScope.launch {
            val result = gameRepository.joinGame(game.id, user.id)

            requireActivity().runOnUiThread {
                progressBar.visibility = View.GONE

                if (result.isSuccess) {
                    Toast.makeText(context, "Joined game!", Toast.LENGTH_SHORT).show()
                    // Launch WebView activity instead of fragment navigation
                    val intent = Intent(requireContext(), WebViewGameActivity::class.java).apply {
                        putExtra("gameId", game.id)
                    }
                    startActivity(intent)
                } else {
                    Toast.makeText(
                        context,
                        result.exceptionOrNull()?.message ?: getString(R.string.error_join_game),
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
        }
    }

    override fun onResume() {
        super.onResume()
        loadGames()
    }
}

class GamesAdapter(
    private val onJoinClick: (Game) -> Unit
) : RecyclerView.Adapter<GamesAdapter.GameViewHolder>() {

    private var games = emptyList<Game>()

    fun submitList(newGames: List<Game>) {
        games = newGames
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): GameViewHolder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_game, parent, false)
        return GameViewHolder(view)
    }

    override fun onBindViewHolder(holder: GameViewHolder, position: Int) {
        holder.bind(games[position], onJoinClick)
    }

    override fun getItemCount() = games.size

    class GameViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val playerName: TextView = itemView.findViewById(R.id.playerName)
        private val gameInfo: TextView = itemView.findViewById(R.id.gameInfo)
        private val joinButton: MaterialButton = itemView.findViewById(R.id.joinButton)

        fun bind(game: Game, onJoinClick: (Game) -> Unit) {
            val creatorName = game.whiteName ?: "Player"
            playerName.text = creatorName
            gameInfo.text = "Waiting for opponent..."

            joinButton.setOnClickListener {
                onJoinClick(game)
            }
        }
    }
}
