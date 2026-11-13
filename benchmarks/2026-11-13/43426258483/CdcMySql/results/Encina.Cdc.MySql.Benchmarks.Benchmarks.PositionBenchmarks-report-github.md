```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.018 ns |   0.6446 ns |  0.0353 ns |   0.48 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.311 ns |   0.7262 ns |  0.0398 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   766.241 ns | 141.0357 ns |  7.7306 ns |  92.19 |    0.89 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,196.475 ns | 210.6525 ns | 11.5466 ns | 143.96 |    1.34 | 0.0286 |     504 B |       12.60 |
