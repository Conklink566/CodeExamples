using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame
{
    public class PathFinderManager : MonoBehaviour
    {
        public class PathNode
        {
            public GridCoordinates currentPos;
            public GridCoordinates destinationPos;
            public int gCost;
            public int hCost;
            public int fCost { get { return gCost + hCost; } }
            public PathNode parentNode;
            public const int childCreationCount = 4;
            public bool closedNode = false;
            public bool unstepable = false;
            public bool usableNode = true;

            public PathNode(GridCoordinates currentPosition, GridCoordinates destination, PathNode parentNode = null)
            {
                currentPos = currentPosition;
                destinationPos = destination;
                this.parentNode = parentNode;
                gCost = parentNode == null ? 0 : parentNode.gCost + 1;
                hCost = Mathf.Abs(currentPosition.row - destination.row) + Mathf.Abs(currentPosition.column - destination.column);
            }

            public PathNode()
            {
                usableNode = false;
            }

            public PathNode [] CreateChildren(GridCoordinates gridDimensions)
            {
                GridCoordinates [] childrenPos = new GridCoordinates [4];
                PathNode [] childrenNodes = new PathNode [4];
                for (int i = 0; i < childCreationCount; i++)
                {
                    GridCoordinates childPosition = new GridCoordinates(this.currentPos.row, this.currentPos.column);
                    if (i == 0)
                        childPosition.column++;
                    else if (i == 1)
                        childPosition.column--;
                    else if (i == 2)
                        childPosition.row++;
                    else if (i == 3)
                        childPosition.row--;
                    if (gridDimensions.row <= childPosition.row ||
                       childPosition.row < 0 ||
                       gridDimensions.column <= childPosition.column ||
                       childPosition.column < 0)
                    {
                        childPosition.invalid = true;
                    }
                    PathNode childNode = new PathNode(childPosition, destinationPos, this);
                    childrenNodes [i] = childNode;
                }
                return childrenNodes;
            }
        }

        public PathNode [,] pathNodeGrid;

        public static PathFinderManager Instance = null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }


        public static void SetupNodeGrid(GridTile[][] m_gridConfiguration)
        {
            Instance.pathNodeGrid = new PathNode [m_gridConfiguration [0].Length, m_gridConfiguration.Length];
            GridCoordinates tilePlacer = GridCoordinates.New;
            for (int y = m_gridConfiguration.Length - 1; y >= 0; y--)
            {
                for (int x = 0; x < m_gridConfiguration[y].Length; x++)
                {
                    if (m_gridConfiguration[y] [x].tileType == GridTile.TileType.Empty ||
                       (TileGridManager.GetTileObject(tilePlacer.row, tilePlacer.column, CoreGameManager.GetSelectedPuzzlePlayer().currentPos.floor).tileAttribute != null &&
                        TileGridManager.GetTileObject(tilePlacer.row, tilePlacer.column, CoreGameManager.GetSelectedPuzzlePlayer().currentPos.floor).tileAttribute.canEnter == false))
                    {
                        Instance.pathNodeGrid[tilePlacer.row, tilePlacer.column] = new PathNode();
                    }
                    tilePlacer.row++;
                }
                tilePlacer.row = 0;
                tilePlacer.column++;
            }
        }


        public static List<PathNode> FindPath(GridCoordinates startingDestination, GridCoordinates endDestination, GridCoordinates gridDimensions)
        {
            SetupNodeGrid(TileGridManager.Instance.gridConfiguration[(TileGridManager.Instance.gridConfiguration.Length - 1) - endDestination.floor]);
            List<PathNode> OpenNodes = new List<PathNode>();
            List<PathNode> DesiredPathNodes = new List<PathNode>();
            PathNode currentNode = null;
            PathNode original = new PathNode(startingDestination, endDestination);
            currentNode = original;
            Instance.pathNodeGrid [currentNode.currentPos.row, currentNode.currentPos.column] = currentNode;
            bool pathCreated = false;
            while(pathCreated == false)
            {
                //Create child nodes from the current node
                PathNode[] childNodes = currentNode.CreateChildren(gridDimensions);
                for(int i = 0; i < childNodes.Length; i++)
                {
                    if (childNodes [i].currentPos.invalid == true)
                        continue;
                    if(Instance.pathNodeGrid[childNodes[i].currentPos.row, childNodes [i].currentPos.column] != null &&
                       Instance.pathNodeGrid[childNodes[i].currentPos.row, childNodes[i].currentPos.column].usableNode == false)
                    {
                        continue;
                    }
                    if(Instance.pathNodeGrid [childNodes [i].currentPos.row, childNodes [i].currentPos.column] == null)
                    {
                        Instance.pathNodeGrid [childNodes [i].currentPos.row, childNodes [i].currentPos.column] = childNodes [i];
                        if (childNodes [i].currentPos == endDestination)
                        {
                            pathCreated = true;
                            currentNode = childNodes [i];
                            break;
                        }
                        OpenNodes.Add(childNodes [i]);
                        continue;
                    }
                    if(Instance.pathNodeGrid [childNodes [i].currentPos.row, childNodes [i].currentPos.column].closedNode == true)
                    {
                        continue;
                    }
                    if(Instance.pathNodeGrid [childNodes [i].currentPos.row, childNodes [i].currentPos.column].fCost > childNodes[i].fCost)
                    {
                        Instance.pathNodeGrid [childNodes [i].currentPos.row, childNodes [i].currentPos.column] = childNodes [i];
                    }
                    else
                    {
                        continue;
                    }
                    if(childNodes[i].currentPos == endDestination)
                    {
                        pathCreated = true;
                        currentNode = childNodes [i];
                        break;
                    }
                    else
                    {
                        OpenNodes.Add(childNodes[i]);
                    }
                }
                //Check to see if we can connected to the path
                if(pathCreated == true)
                {
                    bool extractAllParents = true;
                    while(extractAllParents == true)
                    {
                        bool foundParent = currentNode.parentNode != null;
                        currentNode.currentPos.floor = endDestination.floor;
                        DesiredPathNodes.Add(currentNode);
                        if(foundParent == true)
                        {
                            currentNode = currentNode.parentNode;
                            continue;
                        }
                        extractAllParents = false;
                    }
                    break;
                }
                //Set new current node
                Instance.pathNodeGrid [currentNode.currentPos.row, currentNode.currentPos.column].closedNode = true;
                currentNode = null;
                for(int i = 0; i < OpenNodes.Count; i++)
                {
                    if(OpenNodes[i] == null)
                    {
                        continue;
                    }
                    if(Instance.pathNodeGrid[OpenNodes[i].currentPos.row, OpenNodes[i].currentPos.column] == null)
                    {
                        pathCreated = true;
                        break;
                    }
                    if(Instance.pathNodeGrid [OpenNodes [i].currentPos.row, OpenNodes [i].currentPos.column].closedNode == true)
                    {
                        OpenNodes [i] = null;
                        continue;
                    }
                    if(currentNode == null ||
                        currentNode.fCost > OpenNodes[i].fCost)
                    {
                        currentNode = OpenNodes [i];
                    }
                }
                if(currentNode == null)
                {
                    pathCreated = true;
                    DesiredPathNodes = null;
                }
            }
            return DesiredPathNodes;
        }
    }
}