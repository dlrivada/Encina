```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.07 ns |     7.235 ns |   0.397 ns |     1.40 |    0.02 | 0.0044 |     112 B |        1.00 |
| DirectCall              |     28.54 ns |     8.625 ns |   0.473 ns |     1.00 |    0.02 | 0.0045 |     112 B |        1.00 |
| ExpressionCompilation   | 62,659.39 ns | 8,026.027 ns | 439.934 ns | 2,195.72 |   34.49 | 0.1221 |    5270 B |       47.05 |
| GenericTypeConstruction |    126.37 ns |    11.146 ns |   0.611 ns |     4.43 |    0.07 | 0.0069 |     176 B |        1.57 |
| MethodInfoInvoke        |     84.62 ns |     3.396 ns |   0.186 ns |     2.97 |    0.04 | 0.0069 |     176 B |        1.57 |
