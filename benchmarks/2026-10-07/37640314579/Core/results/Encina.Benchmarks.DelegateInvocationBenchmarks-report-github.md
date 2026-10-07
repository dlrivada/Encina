```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.59GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error      | StdDev   | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|---------:|---------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.75 ns |   2.735 ns | 0.150 ns |     1.41 |    0.02 | 0.0044 |     112 B |        1.00 |
| DirectCall              |     27.51 ns |   6.038 ns | 0.331 ns |     1.00 |    0.01 | 0.0045 |     112 B |        1.00 |
| ExpressionCompilation   | 62,510.05 ns | 126.577 ns | 6.938 ns | 2,272.73 |   23.84 | 0.1221 |    5270 B |       47.05 |
| GenericTypeConstruction |    126.04 ns |   4.674 ns | 0.256 ns |     4.58 |    0.05 | 0.0069 |     176 B |        1.57 |
| MethodInfoInvoke        |     86.21 ns |   4.018 ns | 0.220 ns |     3.13 |    0.03 | 0.0069 |     176 B |        1.57 |
