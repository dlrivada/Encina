```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.825 ns |  0.4437 ns | 0.0243 ns |  0.34 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   8.218 ns |  1.3897 ns | 0.0762 ns |  1.00 |    0.01 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 542.947 ns | 23.3450 ns | 1.2796 ns | 66.07 |    0.55 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 625.662 ns | 23.7793 ns | 1.3034 ns | 76.13 |    0.63 | 0.0057 |     504 B |       12.60 |
