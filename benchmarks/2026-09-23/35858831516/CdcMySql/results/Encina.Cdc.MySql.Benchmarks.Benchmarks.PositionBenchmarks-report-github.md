```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.979 ns |  0.0421 ns | 0.0023 ns |   0.43 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.220 ns |  3.6295 ns | 0.1989 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   785.533 ns | 15.8583 ns | 0.8692 ns |  85.22 |    1.61 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,166.238 ns | 28.5720 ns | 1.5661 ns | 126.53 |    2.40 | 0.0286 |     504 B |       12.60 |
