```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.974 ns |  0.0777 ns | 0.0043 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.506 ns |  2.1289 ns | 0.1167 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   805.028 ns | 25.0365 ns | 1.3723 ns |  94.65 |    1.13 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,192.272 ns | 38.3495 ns | 2.1021 ns | 140.19 |    1.68 | 0.0286 |     504 B |       12.60 |
