```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.666 ns |  0.3504 ns | 0.0192 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.910 ns |  5.2835 ns | 0.2896 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 555.369 ns | 37.3461 ns | 2.0471 ns |  80.47 |    2.86 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 827.433 ns | 98.7588 ns | 5.4133 ns | 119.89 |    4.30 | 0.0296 |     504 B |       12.60 |
