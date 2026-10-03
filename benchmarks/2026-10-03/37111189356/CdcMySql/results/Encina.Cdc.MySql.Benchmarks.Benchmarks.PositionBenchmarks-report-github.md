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
| CompareFilePositions |   2.810 ns |  0.0798 ns | 0.0044 ns |   0.42 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.710 ns |  3.4235 ns | 0.1877 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 548.426 ns | 66.4450 ns | 3.6421 ns |  81.77 |    2.02 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 834.414 ns |  8.3328 ns | 0.4568 ns | 124.41 |    2.98 | 0.0296 |     504 B |       12.60 |
