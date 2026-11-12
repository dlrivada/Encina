```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.675 ns |   0.4269 ns |  0.0234 ns |   0.39 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.891 ns |   6.0571 ns |  0.3320 ns |   1.00 |    0.06 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 572.630 ns | 390.6269 ns | 21.4116 ns |  83.22 |    4.33 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 841.318 ns |   1.3521 ns |  0.0741 ns | 122.28 |    4.98 | 0.0296 |     504 B |       12.60 |
