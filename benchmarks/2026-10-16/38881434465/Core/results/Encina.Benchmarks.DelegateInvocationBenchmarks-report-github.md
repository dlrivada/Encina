```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.94 ns |     1.478 ns |   0.081 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.39 ns |     1.876 ns |   0.103 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,299.67 ns | 4,001.016 ns | 219.309 ns | 2,380.24 |   12.70 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    105.02 ns |     6.152 ns |   0.337 ns |     4.69 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     71.34 ns |     7.549 ns |   0.414 ns |     3.19 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
