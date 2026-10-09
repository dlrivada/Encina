```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.977 ns |  0.0304 ns | 0.0017 ns |   0.43 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.316 ns |  0.9676 ns | 0.0530 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   788.843 ns | 21.7909 ns | 1.1944 ns |  84.68 |    0.43 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,169.840 ns | 41.3558 ns | 2.2669 ns | 125.57 |    0.66 | 0.0286 |     504 B |       12.60 |
