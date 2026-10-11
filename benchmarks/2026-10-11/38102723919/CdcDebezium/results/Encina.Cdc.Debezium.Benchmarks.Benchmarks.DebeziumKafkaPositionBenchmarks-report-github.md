```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error      | StdDev     | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------:|----------:|
| ComparePositions |     2.682 ns |  0.0045 ns |  0.0067 ns |      - |         - |
| FromBytes        | 1,476.756 ns | 13.5003 ns | 19.7886 ns | 0.0439 |     744 B |
| ToBytes          |   697.583 ns |  5.5073 ns |  7.8985 ns | 0.0153 |     264 B |
