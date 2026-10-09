```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.23 ns |      4.854 ns |   0.266 ns |     1.36 |    0.01 | 0.0044 |     112 B |        1.00 |
| DirectCall              |     28.81 ns |      5.137 ns |   0.282 ns |     1.00 |    0.01 | 0.0045 |     112 B |        1.00 |
| ExpressionCompilation   | 63,370.06 ns | 13,975.241 ns | 766.030 ns | 2,199.52 |   29.67 | 0.1221 |    5270 B |       47.05 |
| GenericTypeConstruction |    126.65 ns |      3.791 ns |   0.208 ns |     4.40 |    0.04 | 0.0069 |     176 B |        1.57 |
| MethodInfoInvoke        |     85.16 ns |      2.003 ns |   0.110 ns |     2.96 |    0.03 | 0.0069 |     176 B |        1.57 |
