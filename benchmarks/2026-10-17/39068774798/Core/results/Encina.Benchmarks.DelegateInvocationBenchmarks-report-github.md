```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.66 ns |     26.938 ns |   1.477 ns |     1.45 |    0.05 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     27.29 ns |      8.270 ns |   0.453 ns |     1.00 |    0.02 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 31,975.90 ns | 10,915.052 ns | 598.291 ns | 1,172.03 |   25.40 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    129.70 ns |     69.429 ns |   3.806 ns |     4.75 |    0.14 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     84.50 ns |     36.310 ns |   1.990 ns |     3.10 |    0.08 | 0.0020 |     176 B |        1.57 |
