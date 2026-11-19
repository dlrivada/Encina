```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.672 ns |   0.3128 ns |  0.0171 ns |   0.39 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.887 ns |   0.3617 ns |  0.0198 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 563.888 ns |  33.9491 ns |  1.8609 ns |  81.88 |    0.31 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 852.056 ns | 541.4036 ns | 29.6762 ns | 123.72 |    3.74 | 0.0296 |     504 B |       12.60 |
