```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.57 ns |     0.947 ns |   0.052 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.16 ns |     2.119 ns |   0.116 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,307.84 ns | 3,301.141 ns | 180.947 ns | 2,360.65 |   12.82 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    103.92 ns |     6.793 ns |   0.372 ns |     4.69 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     74.39 ns |    37.473 ns |   2.054 ns |     3.36 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
