```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.971 ns |   0.0728 ns | 0.0040 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.318 ns |   2.4790 ns | 0.1359 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   790.077 ns | 138.5966 ns | 7.5969 ns |  95.00 |    1.56 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,147.054 ns |  55.1483 ns | 3.0229 ns | 137.93 |    1.98 | 0.0286 |     504 B |       12.60 |
