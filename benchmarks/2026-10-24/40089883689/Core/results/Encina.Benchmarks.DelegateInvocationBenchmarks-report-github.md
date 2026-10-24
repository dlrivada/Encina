```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.34 ns |    23.647 ns |   1.296 ns |     1.51 |    0.06 | 0.0013 |     112 B |        1.00 |
| DirectCall              |     26.66 ns |    17.430 ns |   0.955 ns |     1.00 |    0.04 | 0.0013 |     112 B |        1.00 |
| ExpressionCompilation   | 32,430.66 ns | 3,121.556 ns | 171.103 ns | 1,217.43 |   37.66 | 0.0610 |    5287 B |       47.21 |
| GenericTypeConstruction |    125.17 ns |    33.036 ns |   1.811 ns |     4.70 |    0.16 | 0.0019 |     176 B |        1.57 |
| MethodInfoInvoke        |     92.69 ns |     1.956 ns |   0.107 ns |     3.48 |    0.11 | 0.0020 |     176 B |        1.57 |
