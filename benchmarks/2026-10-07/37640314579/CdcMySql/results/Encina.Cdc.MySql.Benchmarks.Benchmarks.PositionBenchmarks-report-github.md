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
| CompareFilePositions |     4.008 ns |  0.6852 ns | 0.0376 ns |   0.52 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.680 ns |  0.4696 ns | 0.0257 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   775.113 ns | 85.1984 ns | 4.6700 ns | 100.92 |    0.60 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,151.457 ns | 40.7878 ns | 2.2357 ns | 149.92 |    0.50 | 0.0286 |     504 B |       12.60 |
