```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  2.419 μs | 0.0762 μs | 0.0504 μs |  1.25 |    0.04 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.448 μs | 0.0411 μs | 0.0245 μs |  1.26 |    0.03 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.538 μs | 0.0590 μs | 0.0390 μs |  1.31 |    0.04 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 32.084 μs | 0.3280 μs | 0.2169 μs | 16.53 |    0.42 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  1.943 μs | 0.0771 μs | 0.0510 μs |  1.00 |    0.04 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  4.046 μs | 0.1810 μs | 0.1077 μs |  2.08 |    0.07 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.124 μs | 0.0268 μs | 0.0177 μs |  1.09 |    0.03 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  2.398 μs | 0.9368 μs | 0.0513 μs |  1.25 |    0.02 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  2.387 μs | 0.6622 μs | 0.0363 μs |  1.25 |    0.02 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  2.404 μs | 0.7178 μs | 0.0393 μs |  1.26 |    0.02 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 32.248 μs | 8.8286 μs | 0.4839 μs | 16.87 |    0.24 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  1.912 μs | 0.2341 μs | 0.0128 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  3.982 μs | 0.8842 μs | 0.0485 μs |  2.08 |    0.03 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.119 μs | 0.2989 μs | 0.0164 μs |  1.11 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
