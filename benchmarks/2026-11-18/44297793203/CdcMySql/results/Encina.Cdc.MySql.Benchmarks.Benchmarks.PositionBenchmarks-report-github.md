```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.703 ns |  0.4468 ns | 0.0245 ns |   0.40 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.695 ns |  2.8624 ns | 0.1569 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 538.233 ns | 17.3922 ns | 0.9533 ns |  80.42 |    1.62 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 835.440 ns | 71.2996 ns | 3.9082 ns | 124.82 |    2.55 | 0.0296 |     504 B |       12.60 |
