```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.656 ns |  0.0719 ns | 0.0039 ns |   0.38 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.962 ns |  3.1052 ns | 0.1702 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 565.570 ns | 82.9626 ns | 4.5475 ns |  81.27 |    1.79 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 845.963 ns | 93.2662 ns | 5.1122 ns | 121.56 |    2.62 | 0.0296 |     504 B |       12.60 |
