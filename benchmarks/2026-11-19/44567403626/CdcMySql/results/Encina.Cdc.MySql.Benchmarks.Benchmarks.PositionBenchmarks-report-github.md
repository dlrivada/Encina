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
| CompareFilePositions |   2.738 ns |   1.789 ns | 0.0980 ns |   0.40 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.802 ns |   3.106 ns | 0.1702 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 542.895 ns |  56.814 ns | 3.1142 ns |  79.85 |    1.77 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 839.873 ns | 159.854 ns | 8.7621 ns | 123.53 |    2.89 | 0.0296 |     504 B |       12.60 |
