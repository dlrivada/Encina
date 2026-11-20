```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.783 ns |   1.349 ns |  0.0739 ns |   0.41 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.802 ns |   3.419 ns |  0.1874 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 562.917 ns | 168.751 ns |  9.2498 ns |  82.80 |    2.32 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 842.716 ns | 278.691 ns | 15.2760 ns | 123.96 |    3.57 | 0.0296 |     504 B |       12.60 |
