```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error     | StdDev    | Gen0   | Allocated |
|----------------- |-------------:|----------:|----------:|-------:|----------:|
| ComparePositions |     3.468 ns | 0.0031 ns | 0.0044 ns |      - |         - |
| FromBytes        | 2,007.726 ns | 6.8011 ns | 9.7540 ns | 0.0420 |     744 B |
| ToBytes          |   913.702 ns | 1.1479 ns | 1.6463 ns | 0.0153 |     264 B |
