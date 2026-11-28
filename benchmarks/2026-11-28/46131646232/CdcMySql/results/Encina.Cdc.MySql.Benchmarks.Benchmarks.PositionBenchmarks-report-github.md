```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.660 ns |   0.0542 ns |  0.0030 ns |   0.37 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.107 ns |   4.2936 ns |  0.2353 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 600.202 ns | 297.7042 ns | 16.3182 ns |  84.51 |    3.14 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 846.559 ns |   8.8615 ns |  0.4857 ns | 119.20 |    3.43 | 0.0296 |     504 B |       12.60 |
