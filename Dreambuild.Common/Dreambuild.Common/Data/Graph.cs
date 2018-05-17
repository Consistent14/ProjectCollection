﻿using Dreambuild.Collections;
using Dreambuild.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dreambuild.Data
{
    public class Vertex<TProperty>
    {
        public long ID { get; set; }

        public TProperty Property { get; set; }

        public Vertex(long id, TProperty property = default(TProperty))
        {
            this.ID = id;
            this.Property = property;
        }
    }

    public class Edge<TProperty>
    {
        public long Source { get; set; }

        public long Destination { get; set; }

        public TProperty Property { get; set; }

        public Edge(long source, long destination, TProperty property = default(TProperty))
        {
            this.Source = source;
            this.Destination = destination;
            this.Property = property;
        }
    }

    //public class EdgeTriplet<TSource, TEdge, TDestination> : Edge<TEdge>
    //{
    //    public TSource SourceProperty { get; set; }

    //    public TDestination DestinationProperty { get; set; }

    //    public EdgeTriplet()
    //    {

    //    }
    //}

    public class Graph<TVertex, TEdge>
    {
        public IEnumerable<Vertex<TVertex>> Vertices { get; protected set; }

        public IEnumerable<Edge<TEdge>> Edges { get; protected set; }

        protected Dictionary<long, Vertex<TVertex>> VerticesIndex { get; set; }

        protected DoubleDictionary<long, long, Edge<TEdge>> OutgoingEdges { get; set; }

        protected DoubleDictionary<long, long, Edge<TEdge>> IncomingEdges { get; set; }

        protected Graph()
        {
            this.Vertices = new List<Vertex<TVertex>>();
            this.Edges = new List<Edge<TEdge>>();
            this.VerticesIndex = new Dictionary<long, Vertex<TVertex>>();
            this.OutgoingEdges = new DoubleDictionary<long, long, Edge<TEdge>>();
            this.IncomingEdges = new DoubleDictionary<long, long, Edge<TEdge>>();
        }

        public Graph(IEnumerable<Vertex<TVertex>> vertices, IEnumerable<Edge<TEdge>> edges)
            : this()
        {
            this.Vertices = vertices;
            this.Edges = edges;
            this.VerticesIndex = vertices.ToDictionary(vertex => vertex.ID, vertex => vertex);

            edges.ForEach(edge =>
            {
                this.OutgoingEdges.Add(edge.Source, edge.Destination, edge);
                this.IncomingEdges.Add(edge.Destination, edge.Source, edge);
            });
        }

        public IDictionary<long, int> GetOutgoingDegrees() => this.Vertices.ToDictionary(vertex => vertex.ID, vertex => this.OutgoingEdges[vertex.ID].Count);

        public IDictionary<long, int> GetIncomingDegrees() => this.Vertices.ToDictionary(vertex => vertex.ID, vertex => this.IncomingEdges[vertex.ID].Count);

        public IDictionary<long, int> GetDegrees() => this.Vertices.ToDictionary(vertex => vertex.ID, vertex => this.OutgoingEdges[vertex.ID].Count + this.IncomingEdges[vertex.ID].Count);
    }

    public class GraphBuilder
    {

    }
}