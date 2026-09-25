```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.546 ns |  1.376 ns | 0.0754 ns |   0.40 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   8.958 ns |  2.761 ns | 0.1513 ns |   1.00 |    0.02 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 803.451 ns | 19.843 ns | 1.0877 ns |  89.70 |    1.32 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 903.906 ns | 10.988 ns | 0.6023 ns | 100.92 |    1.48 | 0.0057 |     504 B |       12.60 |
