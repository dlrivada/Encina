```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.42 ns |     3.552 ns |   0.195 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.59 ns |     0.276 ns |   0.015 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 51,416.14 ns | 4,748.818 ns | 260.299 ns | 2,381.32 |   10.54 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |     99.31 ns |     5.281 ns |   0.289 ns |     4.60 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     69.65 ns |    37.829 ns |   2.074 ns |     3.23 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
