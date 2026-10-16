```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.02 ns |     0.861 ns |   0.047 ns |     1.23 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.02 ns |     8.014 ns |   0.439 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,374.66 ns | 1,927.042 ns | 105.628 ns | 2,379.02 |   40.84 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    103.23 ns |     7.028 ns |   0.385 ns |     4.69 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.14 ns |     4.284 ns |   0.235 ns |     2.96 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
