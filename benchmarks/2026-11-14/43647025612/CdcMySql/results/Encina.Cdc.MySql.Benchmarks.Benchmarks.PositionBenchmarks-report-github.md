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
| CompareFilePositions |   2.804 ns |  2.4630 ns | 0.1350 ns |   0.42 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.711 ns |  0.3371 ns | 0.0185 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 551.242 ns | 46.7255 ns | 2.5612 ns |  82.14 |    0.38 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 834.383 ns | 40.5951 ns | 2.2252 ns | 124.33 |    0.41 | 0.0296 |     504 B |       12.60 |
