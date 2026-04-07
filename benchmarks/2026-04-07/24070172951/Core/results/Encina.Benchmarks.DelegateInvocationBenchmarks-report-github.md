```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     39.09 ns |      6.427 ns |   0.352 ns |     0.99 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     39.30 ns |      7.352 ns |   0.403 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,324.55 ns | 10,173.192 ns | 557.627 ns | 2,324.14 |   23.92 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.91 ns |      7.299 ns |   0.400 ns |     3.36 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.69 ns |      1.711 ns |   0.094 ns |     2.18 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
