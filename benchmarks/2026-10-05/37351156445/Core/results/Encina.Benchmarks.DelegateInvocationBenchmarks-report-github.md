```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     43.87 ns |    19.956 ns |   1.094 ns |   1.40 |    0.03 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     31.37 ns |     6.079 ns |   0.333 ns |   1.00 |    0.01 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 31,187.46 ns | 1,897.169 ns | 103.990 ns | 994.21 |    9.61 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    128.61 ns |     3.014 ns |   0.165 ns |   4.10 |    0.04 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     89.35 ns |     9.012 ns |   0.494 ns |   2.85 |    0.03 | 0.0020 |     176 B |        1.57 |
