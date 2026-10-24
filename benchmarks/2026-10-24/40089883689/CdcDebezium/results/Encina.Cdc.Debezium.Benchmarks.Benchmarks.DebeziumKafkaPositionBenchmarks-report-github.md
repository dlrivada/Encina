```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.71GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | Mean         | Error         | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|--------------:|-----------:|-------:|----------:|
| ComparePositions |     2.928 ns |     0.0658 ns |  0.0036 ns |      - |         - |
| FromBytes        | 2,219.187 ns | 1,084.3140 ns | 59.4349 ns | 0.0267 |     744 B |
| ToBytes          |   982.160 ns |   178.3855 ns |  9.7779 ns | 0.0095 |     264 B |
