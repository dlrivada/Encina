```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   3.042 ns |  0.4155 ns | 0.0228 ns |   0.49 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.170 ns |  1.3909 ns | 0.0762 ns |   1.00 |    0.02 | 0.0005 |      40 B |        1.00 |
| FromBytes            | 664.709 ns | 94.0555 ns | 5.1555 ns | 107.74 |    1.36 | 0.0076 |     688 B |       17.20 |
| ToBytes              | 775.757 ns | 85.3616 ns | 4.6790 ns | 125.74 |    1.50 | 0.0057 |     504 B |       12.60 |
