```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.971 ns |  0.0531 ns | 0.0029 ns |   0.47 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.361 ns |  0.8142 ns | 0.0446 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   752.023 ns | 16.9292 ns | 0.9279 ns |  89.95 |    0.43 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,156.928 ns | 48.1800 ns | 2.6409 ns | 138.38 |    0.70 | 0.0286 |     504 B |       12.60 |
