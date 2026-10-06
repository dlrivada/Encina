```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     44.97 ns |     2.627 ns |  0.144 ns |   1.37 |    0.00 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     32.93 ns |     1.652 ns |  0.091 ns |   1.00 |    0.00 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 31,378.22 ns | 1,193.176 ns | 65.402 ns | 952.95 |    2.85 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    127.93 ns |     1.268 ns |  0.070 ns |   3.89 |    0.01 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     92.59 ns |     2.873 ns |  0.157 ns |   2.81 |    0.01 | 0.0020 |     176 B |        1.57 |
