```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method           | Mean         | Error      | StdDev     | Median       | Gen0   | Allocated |
|----------------- |-------------:|-----------:|-----------:|-------------:|-------:|----------:|
| ComparePositions |     3.469 ns |  0.0049 ns |  0.0069 ns |     3.467 ns |      - |         - |
| FromBytes        | 2,044.703 ns | 20.9037 ns | 30.6404 ns | 2,034.301 ns | 0.0420 |     744 B |
| ToBytes          |   936.536 ns | 14.1700 ns | 20.7702 ns |   954.139 ns | 0.0153 |     264 B |
