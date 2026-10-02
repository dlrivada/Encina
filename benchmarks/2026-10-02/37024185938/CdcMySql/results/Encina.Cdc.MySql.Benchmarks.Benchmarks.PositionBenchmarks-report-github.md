```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.972 ns |   0.3971 ns | 0.0218 ns |   0.39 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   5.009 ns |   1.8696 ns | 0.1025 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 459.746 ns |  78.2482 ns | 4.2890 ns |  91.80 |    1.77 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 676.777 ns | 107.8420 ns | 5.9112 ns | 135.14 |    2.58 | 0.0296 |     504 B |       12.60 |
