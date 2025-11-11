package com.example.mobile;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;
import java.util.ArrayList;
import java.util.List;

public class ConversationAdapter extends RecyclerView.Adapter<ConversationAdapter.ConversationViewHolder> {

    private List<Conversation> conversationList = new ArrayList<>();
    private final OnItemClickListener listener;

    public interface OnItemClickListener {
        void onItemClick(Conversation conversation);
    }

    public ConversationAdapter(OnItemClickListener listener) {
        this.listener = listener;
    }

    @NonNull
    @Override
    public ConversationViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View itemView = LayoutInflater.from(parent.getContext()).inflate(R.layout.list_item_chat, parent, false);
        return new ConversationViewHolder(itemView);
    }

    @Override
    public void onBindViewHolder(@NonNull ConversationViewHolder holder, int position) {
        Conversation currentConversation = conversationList.get(position);
        holder.bind(currentConversation, listener);
    }

    @Override
    public int getItemCount() {
        return conversationList.size();
    }

    public void setConversations(List<Conversation> conversations) {
        this.conversationList = conversations;
        notifyDataSetChanged();
    }

    static class ConversationViewHolder extends RecyclerView.ViewHolder {
        private final TextView textViewProfessionalName;
        private final TextView textViewLastMessage; // Usaremos este campo para o título do ticket

        public ConversationViewHolder(@NonNull View itemView) {
            super(itemView);
            textViewProfessionalName = itemView.findViewById(R.id.textViewProfessionalName);
            textViewLastMessage = itemView.findViewById(R.id.textViewLastMessage);
        }

        public void bind(final Conversation conversation, final OnItemClickListener listener) {
            textViewProfessionalName.setText(conversation.getProfessionalName());
            textViewLastMessage.setText("Ticket: " + conversation.getTicketTitle());
            itemView.setOnClickListener(v -> listener.onItemClick(conversation));
        }
    }
}