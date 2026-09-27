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
| CompareFilePositions |     3.974 ns |  0.0276 ns | 0.0015 ns |   0.47 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.524 ns |  0.5910 ns | 0.0324 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   769.891 ns | 61.5239 ns | 3.3723 ns |  90.32 |    0.45 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,140.405 ns | 68.4288 ns | 3.7508 ns | 133.78 |    0.58 | 0.0286 |     504 B |       12.60 |
