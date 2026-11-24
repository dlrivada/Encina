```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.576 ns |   0.7097 ns |  0.0389 ns |   0.35 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.506 ns |   0.8439 ns |  0.0463 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 448.926 ns | 174.0970 ns |  9.5428 ns |  99.63 |    2.04 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 659.677 ns | 352.3820 ns | 19.3152 ns | 146.40 |    3.93 | 0.0296 |     504 B |       12.60 |
