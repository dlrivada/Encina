```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.675 ns |   0.6339 ns |  0.0347 ns |   0.35 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.569 ns |   5.0747 ns |  0.2782 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 569.817 ns |  20.6431 ns |  1.1315 ns |  75.35 |    2.37 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 872.058 ns | 288.9464 ns | 15.8381 ns | 115.31 |    4.05 | 0.0296 |     504 B |       12.60 |
