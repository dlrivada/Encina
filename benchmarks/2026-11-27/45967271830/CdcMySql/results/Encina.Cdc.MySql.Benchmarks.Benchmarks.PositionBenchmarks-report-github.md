```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.675 ns |  0.5884 ns | 0.0323 ns |   0.39 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.953 ns |  8.6128 ns | 0.4721 ns |   1.00 |    0.08 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 543.449 ns | 61.4583 ns | 3.3687 ns |  78.41 |    4.77 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 870.116 ns | 13.7857 ns | 0.7556 ns | 125.54 |    7.60 | 0.0296 |     504 B |       12.60 |
