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
| CompareFilePositions |     3.990 ns |  0.5707 ns | 0.0313 ns |   0.45 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.907 ns |  3.4132 ns | 0.1871 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   788.836 ns | 55.1166 ns | 3.0211 ns |  88.59 |    1.65 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,156.474 ns | 48.3100 ns | 2.6480 ns | 129.88 |    2.39 | 0.0286 |     504 B |       12.60 |
