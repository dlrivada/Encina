```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.99 ns |     0.721 ns |   0.040 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.73 ns |     2.723 ns |   0.149 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 54,985.19 ns | 3,881.327 ns | 212.749 ns | 2,529.88 |   17.22 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    106.02 ns |    15.331 ns |   0.840 ns |     4.88 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     67.91 ns |     9.253 ns |   0.507 ns |     3.12 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
