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
| CompareFilePositions |   2.669 ns |  0.3037 ns | 0.0166 ns |   0.38 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.050 ns |  2.2153 ns | 0.1214 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 579.605 ns | 62.2311 ns | 3.4111 ns |  82.22 |    1.28 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 841.135 ns | 28.4928 ns | 1.5618 ns | 119.33 |    1.77 | 0.0296 |     504 B |       12.60 |
