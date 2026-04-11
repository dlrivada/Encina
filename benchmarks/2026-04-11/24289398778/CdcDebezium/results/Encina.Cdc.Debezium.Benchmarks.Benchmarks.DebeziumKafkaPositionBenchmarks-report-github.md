```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error      | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------:|----------:|
| ComparePositions |     2.839 ns |  0.0018 ns |  0.0025 ns |      - |         - |
| FromBytes        | 2,217.083 ns | 24.6004 ns | 36.8208 ns | 0.0267 |     744 B |
| ToBytes          |   915.767 ns |  1.8814 ns |  2.7577 ns | 0.0105 |     264 B |
