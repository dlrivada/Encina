```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.041 ns |  1.8727 ns | 0.1027 ns |   0.50 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.067 ns |  0.4467 ns | 0.0245 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   780.224 ns | 82.5672 ns | 4.5258 ns |  96.72 |    0.55 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,175.654 ns | 65.0385 ns | 3.5650 ns | 145.73 |    0.54 | 0.0286 |     504 B |       12.60 |
